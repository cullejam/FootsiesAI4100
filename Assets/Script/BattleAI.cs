using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Footsies
{

    public class BattleAI
    {
        public class FightState
        {
            public float distanceX;
            public bool isOpponentDamage;
            public bool isOpponentGuardBreak;
            public bool isOpponentBlocking;
            public bool isOpponentNormalAttack;
            public bool isOpponentSpecialAttack;
        }

        public class movementReward{
            public string type;
            public double reward;
            public FightState playerMove;
        }

        public class attackReward{
            public string type;
            public double reward;
            public FightState playerMove;
        }

        private BattleCore battleCore;

        private Queue<int> moveQueue = new Queue<int>();
        private Queue<int> attackQueue = new Queue<int>();

        // previous fight state data
        private FightState[] fightStates = new FightState[maxFightStateRecord];
        public static readonly uint maxFightStateRecord = 10;
        private int fightStateReadIndex = 5;

        public BattleAI(BattleCore core)
        {
            battleCore = core;
        }

        public int getNextAIInput(movementReward movement, attackReward attack)
        {
            int input = 0;

            UpdateFightState();
            var fightState = GetCurrentFightState();
            if (fightState != null)
            {
                //Debug.Log(fightState.distanceX);
                if (moveQueue.Count > 0)
                    input |= moveQueue.Dequeue();
                else if (moveQueue.Count == 0)
                {
                    SelectMovement(movement);
                }

                if (attackQueue.Count > 0)
                    input |= attackQueue.Dequeue();
                else if (attackQueue.Count == 0)
                {
                    SelectAttack(attack);
                }

                //Flow for future?:
                /*if (moveQueue.Count > 0)
                    input |= moveQueue.Dequeue();
                if (attackQueue.Count > 0)
                    input |= attackQueue.Dequeue();
                if (attackQueue.Count == 0 && moveQueue.Count > 0)
                {
                    //This method will then call select attack from within it, passing along
                    //the movement which was selected
                    SelectMovement(fightState);
                }*/
            }

            return input;
        }

        public FightState getCurrentState() {
          var fightState = GetCurrentFightState();
          return fightState;
        }

        public movementReward getInitialMovementReward(FightState fightState, List<movementReward> previousRewards) {
          //we are given a specific distance and player move, and here we will randomly choose an AI move
          //Idea: if we already know a move which gives >1 reward for this approximate distance and player move
          //do that AI move, else do a random move and then update the reward for that move given distance and player move
          //FightState newFightState = new FightState();
          movementReward moveReward = new movementReward();
          bool isChosen = false;
          moveReward.playerMove = fightState;
          moveReward.reward = 0;
          for(int i = 0; i < previousRewards.Count; i++) {
            movementReward previous = previousRewards[i];

            if(previous.reward > moveReward.reward && (previous.playerMove.distanceX > fightState.distanceX - 0.5f 
            && previous.playerMove.distanceX < fightState.distanceX + 0.5f) 
            && (previous.playerMove.isOpponentBlocking == fightState.isOpponentBlocking 
            && previous.playerMove.isOpponentDamage == fightState.isOpponentDamage 
            && previous.playerMove.isOpponentGuardBreak == fightState.isOpponentGuardBreak 
            && previous.playerMove.isOpponentNormalAttack == fightState.isOpponentNormalAttack 
            && previous.playerMove.isOpponentSpecialAttack == fightState.isOpponentSpecialAttack)) {
              moveReward = previous;
              isChosen = true;
            }
          }
          //This below means we didn't find a comparable state in the previous states, so time to be random
          //Need to update these rewards so that they can be used
          if(!isChosen) {
            var rand = Random.Range(0, 7);
            if(rand == 0) {
              moveReward.type = "FallBack1";
            }
            else if(rand == 1) {
              moveReward.type = "FallBack2";
            }
            else if(rand == 2) {
              moveReward.type = "MidApproach1";
            }
            else if(rand == 3) {
              moveReward.type = "MidApproach2";
            }
            else if(rand == 4) {
              moveReward.type = "FarApproach1";
            }
            else if(rand == 5) {
              moveReward.type = "FarApproach2";
            }
            else if(rand == 6) {
              moveReward.type = "NeutralMovement";
            }
            else {
              moveReward.type = "NeutralMovement";
            }
          }
          return moveReward;

        }

        public movementReward recalculateMovementReward(FightState playerFightState, movementReward moveReward, attackReward attReward) {
          //This will take the AIs next move and calculate how effective the move truly was
          string aiAttack = attReward.type;
          string aiMove = moveReward.type;
          FightState aiState = updateAIFightState();
          double newReward = 0;
         
          if(aiState.isOpponentBlocking && playerFightState.isOpponentSpecialAttack && playerFightState.distanceX < 2f) {
            newReward = 2;
          }
          else if(aiState.isOpponentBlocking && playerFightState.isOpponentNormalAttack && playerFightState.distanceX < 2f) {
            newReward = 1.8;
          }
          else if(aiState.isOpponentGuardBreak && playerFightState.isOpponentSpecialAttack && playerFightState.distanceX < 2f) {
            newReward = -1;
          }
          else if(aiState.isOpponentGuardBreak && playerFightState.isOpponentNormalAttack && playerFightState.distanceX < 2f) {
            newReward = -1;
          }
          else if(getAIPositionX() > 4.5) {
            newReward = -0.3;
          }
          else if(getAIPositionX() < 0) {
            newReward = -1 * (getAIPositionX() / 2);
          }
          
          moveReward.playerMove = playerFightState;
          moveReward.reward = newReward;
          return moveReward;

        }

        private void SelectMovement(movementReward movement)
        {
            //computeNextMovement(fightState);
            if(movement.type == "FallBack1") {
              AddFallBack1();
            }
            else if(movement.type == "FallBack2") {
              AddFallBack2();
            }
            else if(movement.type == "MidApproach1") {
              AddMidApproach1();
            }
            else if(movement.type == "MidApproach2") {
              AddMidApproach2();
            }
            else if(movement.type == "FarApproach1") {
              AddFarApproach1();
            }
            else if(movement.type == "FarApproach2") {
              AddFarApproach2();
            }
            else if(movement.type == "NeutralMovement") {
              AddNeutralMovement();
            }
            else {
              AddFallBack1();
            }
        }

        public attackReward getInitialAttackReward(FightState fightState, List<attackReward> previousRewards) {
          //we are given a specific distance and player move, and here we will randomly choose an AI move
          //Idea: if we already know a move which gives >1 reward for this approximate distance and player move
          //do that AI move, else do a random move and then update the reward for that move given distance and player move
          //FightState newFightState = new FightState();
          attackReward attReward = new attackReward();
          bool isChosen = false;
          attReward.playerMove = fightState;
          attReward.reward = 0;
          for(int i = 0; i < previousRewards.Count; i++) {
            attackReward previous = previousRewards[i];

            if(previous.reward >= attReward.reward && (previous.playerMove.distanceX > fightState.distanceX - 0.5f 
            && previous.playerMove.distanceX < fightState.distanceX + 0.5f) 
            && (previous.playerMove.isOpponentBlocking == fightState.isOpponentBlocking 
            && previous.playerMove.isOpponentDamage == fightState.isOpponentDamage 
            && previous.playerMove.isOpponentGuardBreak == fightState.isOpponentGuardBreak 
            && previous.playerMove.isOpponentNormalAttack == fightState.isOpponentNormalAttack 
            && previous.playerMove.isOpponentSpecialAttack == fightState.isOpponentSpecialAttack)) {
              attReward = previous;
              isChosen = true;
            }
          }
          //This below means we didn't find a comparable state in the previous states, so time to be random
          //Need to update these rewards so that they can be used
          if(!isChosen) {
            var rand = Random.Range(0, 4);
            if(rand == 0) {
              attReward.type = "OneHitImmediate";
            }
            else if(rand == 1) {
              attReward.type = "TwoHitImmediate";
            }
            else if(rand == 2) {
              attReward.type = "NoAttack";
            }
            else if(rand == 3) {
              attReward.type = "DelaySpecial";
            }
            else if(rand == 4) {
              attReward.type = "ImmediateSpecial";
            }
            else {
              attReward.type = "NoAttack";
            }
          }
          return attReward;

        }

        public attackReward recalculateAttackReward(FightState playerFightState, attackReward attReward, movementReward moveReward) {
          //This will take the AIs next move and calculate how effective the move truly was
          //fightState is the player
          string aiAttack = attReward.type;
          string aiMove = moveReward.type;
          FightState aiState = updateAIFightState();
          double newReward = 0;

          if(playerFightState.isOpponentDamage) {
            newReward = 2;
          }
          else if(playerFightState.isOpponentGuardBreak) {
            newReward = 1.8;
          }
          else if((aiAttack == "OneHitImmediate") && (aiMove != "NeutralMovement") && (aiState.distanceX > 2f)) {
            newReward = 1.1;
          }
          else if((aiAttack == "OneHitImmediate") && (aiMove == "NeutralMovement") && (aiState.distanceX > 3f)) {
            newReward = 1.2;
          }
          else if(aiState.isOpponentDamage) {
            newReward = -2;
          }
          else if(aiState.isOpponentGuardBreak) {
            newReward = -1.8;
          }
          else if(aiAttack == "NoAttack") {
            newReward = -0.1; 
          }
          else if((aiAttack == "OneHitImmediate") && (aiMove != "NeutralMovement") && (aiState.distanceX < 2f)) {
            newReward = 0.3;
          }
          else if((aiAttack == "OneHitImmediate") && (aiMove == "NeutralMovement") && (aiState.distanceX < 3f)) {
            newReward = 0.5;
          }
          else {
            newReward = 0;
          }

          attReward.playerMove = playerFightState;
          attReward.reward = newReward;
          return attReward;

        }

        private void SelectAttack(attackReward attack)
        {
            //computeNextMovement(fightState);
            if(attack.type == "OneHitImmedaite") {
              AddOneHitImmediateAttack();
            }
            else if(attack.type == "TwoHitImmediate") {
              AddTwoHitImmediateAttack();
            }
            else if(attack.type == "NoAttack") {
              AddNoAttack();
            }
            else if(attack.type == "DelaySpecial") {
              AddDelaySpecialAttack();
            }
            else if(attack.type == "ImmedaiteSpecial") {
              AddImmediateSpecialAttack();
            }
            else {
              AddNoAttack();
            }
        }

        private void AddNeutralMovement()
        {
            for (int i = 0; i < 30; i++)
            {
                moveQueue.Enqueue(0);
            }

            Debug.Log("AddNeutral");
        }

        private void AddFarApproach1()
        {
            AddForwardInputQueue(40);
            AddBackwardInputQueue(10);
            AddForwardInputQueue(30);
            AddBackwardInputQueue(10);

            Debug.Log("AddFarApproach1");
        }

        private void AddFarApproach2()
        {
            AddForwardDashInputQueue();
            AddBackwardInputQueue(25);
            AddForwardDashInputQueue();
            AddBackwardInputQueue(25);

            Debug.Log("AddFarApproach2");
        }
        
        private void AddMidApproach1()
        {
            AddForwardInputQueue(30);
            AddBackwardInputQueue(10);
            AddForwardInputQueue(20);
            AddBackwardInputQueue(10);

            Debug.Log("AddMidApproach1");
        }

        private void AddMidApproach2()
        {
            AddForwardDashInputQueue();
            AddBackwardInputQueue(30);

            Debug.Log("AddMidApproach2");
        }

        private void AddFallBack1()
        {
            AddBackwardInputQueue(60);

            Debug.Log("AddFallBack1");
        }

        private void AddFallBack2()
        {
            AddBackwardDashInputQueue();
            AddBackwardInputQueue(60);

            Debug.Log("AddFallBack2");
        }

        private void AddNoAttack()
        {
            for (int i = 0; i < 30; i++)
            {
                attackQueue.Enqueue(0);
            }

            Debug.Log("AddNoAttack");
        }

        private void AddOneHitImmediateAttack()
        {
            attackQueue.Enqueue(GetAttackInput());
            for (int i = 0; i < 18; i++)
            {
                attackQueue.Enqueue(0);
            }

            Debug.Log("AddOneHitImmediateAttack");
        }

        private void AddTwoHitImmediateAttack()
        {
            attackQueue.Enqueue(GetAttackInput());
            for (int i = 0; i < 3; i++)
            {
                attackQueue.Enqueue(0);
            }
            attackQueue.Enqueue(GetAttackInput());
            for (int i = 0; i < 18; i++)
            {
                attackQueue.Enqueue(0);
            }

            Debug.Log("AddTwoHitImmediateAttack");
        }

        private void AddImmediateSpecialAttack()
        {
            for (int i = 0; i < 60; i++)
            {
                attackQueue.Enqueue(GetAttackInput());
            }
            attackQueue.Enqueue(0);

            Debug.Log("AddImmediateSpecialAttack");
        }

        private void AddDelaySpecialAttack()
        {
            for (int i = 0; i < 120; i++)
            {
                attackQueue.Enqueue(GetAttackInput());
            }
            attackQueue.Enqueue(0);

            Debug.Log("AddDelaySpecialAttack");
        }

        private void AddForwardInputQueue(int frame)
        {
            for(int i = 0; i < frame; i++)
            {
                moveQueue.Enqueue(GetForwardInput());
            }
        }

        private void AddBackwardInputQueue(int frame)
        {
            for (int i = 0; i < frame; i++)
            {
                moveQueue.Enqueue(GetBackwardInput());
            }
        }

        private void AddForwardDashInputQueue()
        {
            moveQueue.Enqueue(GetForwardInput());
            moveQueue.Enqueue(0);
            moveQueue.Enqueue(GetForwardInput());
        }

        private void AddBackwardDashInputQueue()
        {
            moveQueue.Enqueue(GetForwardInput());
            moveQueue.Enqueue(0);
            moveQueue.Enqueue(GetForwardInput());
        }

        private FightState updateAIFightState()
        {
            var currentFightState = new FightState();
            currentFightState.distanceX = GetDistanceX();
            currentFightState.isOpponentDamage = battleCore.fighter2.currentActionID == (int)CommonActionID.DAMAGE;
            currentFightState.isOpponentGuardBreak= battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_BREAK;
            currentFightState.isOpponentBlocking = (battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_CROUCH
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_STAND
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.GUARD_M);
            currentFightState.isOpponentNormalAttack = (battleCore.fighter2.currentActionID == (int)CommonActionID.N_ATTACK
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.B_ATTACK);
            currentFightState.isOpponentSpecialAttack = (battleCore.fighter2.currentActionID == (int)CommonActionID.N_SPECIAL
                                                    || battleCore.fighter2.currentActionID == (int)CommonActionID.B_SPECIAL);

            for (int i = 1; i < fightStates.Length; i++)
            {
                fightStates[i] = fightStates[i - 1];
            }
            fightStates[0] = currentFightState;
            return currentFightState;
        }

        private void UpdateFightState()
        {
            var currentFightState = new FightState();
            currentFightState.distanceX = GetDistanceX();
            currentFightState.isOpponentDamage = battleCore.fighter1.currentActionID == (int)CommonActionID.DAMAGE;
            currentFightState.isOpponentGuardBreak= battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_BREAK;
            currentFightState.isOpponentBlocking = (battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_CROUCH
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_STAND
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.GUARD_M);
            currentFightState.isOpponentNormalAttack = (battleCore.fighter1.currentActionID == (int)CommonActionID.N_ATTACK
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.B_ATTACK);
            currentFightState.isOpponentSpecialAttack = (battleCore.fighter1.currentActionID == (int)CommonActionID.N_SPECIAL
                                                    || battleCore.fighter1.currentActionID == (int)CommonActionID.B_SPECIAL);

            for (int i = 1; i < fightStates.Length; i++)
            {
                fightStates[i] = fightStates[i - 1];
            }
            fightStates[0] = currentFightState;
        }

        private FightState GetCurrentFightState()
        {
            return fightStates[fightStateReadIndex];
        }

        private float GetDistanceX()
        {
            return Mathf.Abs(battleCore.fighter2.position.x - battleCore.fighter1.position.x);
        }

        private float getAIPositionX() {
          return battleCore.fighter2.position.x;
        }

        private int GetAttackInput()
        {
            return (int)InputDefine.Attack;
        }

        private int GetForwardInput()
        {
            return (int)InputDefine.Left;
        }

        private int GetBackwardInput()
        {
            return (int)InputDefine.Right;
        }

    }

}
