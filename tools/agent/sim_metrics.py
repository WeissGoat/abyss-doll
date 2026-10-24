"""Prints the tuning dashboard from the run simulation's report.

The nine targets of docs/specs/2026-10-05-demo-tuning-targets.md, plus the loop rate and a few health numbers, read from
UnityClient/Logs/MVPAutoplaytestReport.json (written by P3SmokeTests.MVPAutoplaytestTest.Run; seeds are fixed, so two
runs of the same configs give the same numbers).

    python tools/agent/sim_metrics.py            # one line, for the results table
    python tools/agent/sim_metrics.py --detail   # plus defeats by monster and HP before each boss
"""
import argparse
import collections
import json
import os
import statistics

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
REPORT = os.path.join(ROOT, 'UnityClient', 'Logs', 'MVPAutoplaytestReport.json')
ON_TIME = {'Paid', 'LightDebtAccepted'}


def pct(part, whole):
    return f'{100 * part / whole:.0f}%' if whole else '-'


def mean(values):
    return f'{statistics.mean(values):.0f}' if values else '-'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--report', default=REPORT)
    parser.add_argument('--detail', action='store_true')
    args = parser.parse_args()

    data = json.load(open(args.report, encoding='utf-8-sig'))
    campaigns = data['Campaigns']
    dives = [e for c in campaigns for e in c['ExpeditionReports']]
    l1 = [e for e in dives if e['StartLayerID'] == 1]
    l2 = [e for e in dives if e['StartLayerID'] == 2 or e.get('EnteredLayer2', e['AttemptedLayer2'])]
    l1_won = [e for e in l1 if not e['Defeated']]
    l2_won = [e for e in l2 if e.get('ClearedLayer2', not e['Defeated'])]
    l3 = [e for e in dives if e.get('EnteredLayer3')]
    l3_won = [e for e in l3 if e.get('ClearedLayer3')]
    l1_boss = [e for e in l1 if e.get('HPBeforeBoss', 0) > 0]
    l1_boss_lost = [e for e in l1_boss if (e.get('DefeatedBy') or '').startswith('L1:boss')]

    def rent(day):
        bills = [b for c in campaigns for b in c['RentBills'] if b['Day'] == day]
        return pct(sum(b['Status'] in ON_TIME for b in bills), len(bills)), len(bills)

    rent7, _ = rent(7)
    rent14, bills14 = rent(14)
    income = sum(e.get('NightIncome', 0) for e in dives)
    upkeep = sum(c.get('MaintenanceSpent', 0) for c in campaigns)

    cols = [
        ('1 L1成功', pct(len(l1_won), len(l1))),
        ('2 L1首领败', pct(len(l1_boss_lost), len(l1_boss))),
        ('3 L1营业', mean([e['NightIncome'] for e in l1_won])),
        ('4 第7天房租', rent7),
        ('5 第14天房租', f'{rent14}({bills14})'),
        ('6 破产', pct(sum(c['Bankrupt'] for c in campaigns), len(campaigns))),
        ('7 L2成功', f'{pct(len(l2_won), len(l2))}({len(l2)})'),
        ('8 L2营业', mean([e['NightIncome'] for e in l2_won])),
        ('9 维护/营业', pct(upkeep, income)),
        ('循环率', pct(data['LoopCampaignRate'] * len(campaigns), len(campaigns))),
        ('L2进入', pct(data['Layer2EntryRate'] * len(campaigns), len(campaigns))),
        ('L3进入', pct(data.get('Layer3EntryRate', 0) * len(campaigns), len(campaigns))),
        ('L3成功', f'{pct(len(l3_won), len(l3))}({len(l3)})'),
        ('通关', pct(data.get('Layer3ClearRate', 0) * len(campaigns), len(campaigns))),
        ('底盘升级', str(data['TotalChassisUpgrades'])),
        ('剩余资金', f"{data['AverageFinalMoney']:.0f}"),
    ]
    print(' | '.join(f'{name} {value}' for name, value in cols))

    if args.detail:
        defeats = collections.Counter(e.get('DefeatedBy') for e in dives if e['Defeated'])
        print('defeats:', ', '.join(f'{k} {v}' for k, v in defeats.most_common()))
        print('HP before L1 boss:', mean([e['HPBeforeBoss'] for e in l1_boss]),
              '| HP before L2 boss:', mean([e['HPBeforeBoss'] for e in l2 if e.get('HPBeforeBoss', 0) > 0]),
              '| HP before L3 boss:', mean([e['HPBeforeLayer3Boss'] for e in l3 if e.get('HPBeforeLayer3Boss', 0) > 0]))
        cleared = [c['FirstLayer3ClearDay'] for c in campaigns if c.get('ClearedLayer3')]
        print('first L3 clear day:', mean(cleared), f'({len(cleared)} campaigns)')


if __name__ == '__main__':
    main()
